# OptimusFrame.Transform

Worker Service de processamento de vídeos do sistema OptimusFrame, responsável pela extração de frames de vídeos hospedados no AWS S3, compressão em formato ZIP e publicação de eventos de conclusão via RabbitMQ.

## Visão Geral

O **OptimusFrame.Transform** é um Worker Service (.NET 8) que atua como consumidor de mensagens RabbitMQ no pipeline de processamento de vídeos. Ele recebe notificações de novos vídeos, realiza a extração de frames utilizando OpenCV, comprime os frames em arquivo ZIP e publica o resultado no S3, notificando outros serviços sobre a conclusão do processamento.

## Arquitetura

O projeto segue rigorosamente os princípios de **Clean Architecture** com separação clara de responsabilidades:

```
OptimusFrame.Transform/
├── OptimusFrame.Transform.Application/    # Casos de uso e interfaces
├── OptimusFrame.Transform.Domain/         # Entidades, value objects e exceções
├── OptimusFrame.Transform.Infrastructure/ # Implementações (OpenCV, S3, Compressão)
├── OptimusFrame.Transform.Worker/         # Background Service (RabbitMQ Consumer)
└── OptimusFrame.Transform.sln             # Solução principal
```

### Camadas do Projeto

#### Application Layer (OptimusFrame.Transform.Application)
- **UseCases/ExtractFramesUseCase**: Orquestra todo o processo de extração
  - Download do vídeo do S3
  - Extração de frames
  - Compressão em ZIP
  - Upload do ZIP para S3
- **DTOs**: Request/Response models (ExtractFramesRequest, ExtractFramesResponse)
- **DependencyInjection**: Configuração de injeção de dependências

#### Domain Layer (OptimusFrame.Transform.Domain)
- **Entities/VideoTransform**: Entidade principal de transformação de vídeo
- **Enums/VideoTransformStatus**: Estados do processamento (Pending, Processing, Completed, Failed)
- **ValueObjects**:
  - **FrameExtractionOptions**: Configurações de extração (FPS, qualidade, formato)
  - **S3ObjectKey**: Representação de chave S3 com validação
- **Models/VideoInfo**: Informações de metadados do vídeo
- **Interfaces**: IFrameExtractionService, IStorageService, ICompressionService
- **Exceptions**: DomainException, FrameExtractionException, VideoNotFoundException

#### Infrastructure Layer (OptimusFrame.Transform.Infrastructure)
- **Services/OpenCvFrameExtractionService**: Implementação com OpenCvSharp
  - Extração de frames com configurações customizáveis
  - Suporte para JPEG, PNG e WebP
  - Redimensionamento inteligente com manutenção de proporções
  - Controle de FPS e qualidade
- **Services/ZipCompressionService**: Compressão de diretórios em ZIP
- **Storage/S3StorageService**: Implementação de upload/download S3
- **DependencyInjection**: Configuração de serviços de infraestrutura

#### Worker Layer (OptimusFrame.Transform.Worker)
- **Worker.cs**: Background Service que:
  - Consome mensagens da fila `video.processing.input`
  - Processa vídeos de forma assíncrona
  - Publica resultados na fila `video.processing.completed`
  - Gerencia ACK/NACK de mensagens
- **Messages**: VideoProcessingMessage, VideoProcessingCompletedMessage
- **Configuration**: RabbitMqSettings, StorageSettings

## Tecnologias Utilizadas

- **.NET 8**: Framework principal
- **Worker Service**: Para processamento em background
- **OpenCvSharp4**: Biblioteca para processamento de imagens e vídeos (binding .NET do OpenCV)
- **RabbitMQ.Client**: Cliente para mensageria
- **AWS SDK for .NET (S3)**: Integração com Amazon S3
- **System.IO.Compression**: Compressão de arquivos ZIP
- **Docker**: Containerização da aplicação

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started) (opcional, mas recomendado)
- RabbitMQ Server
- AWS S3 Bucket configurado
- OpenCV (incluído via NuGet package OpenCvSharp4)

## Configuração

### appsettings.json

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "InputQueueName": "video.processing.input",
    "CompletedQueueName": "video.processing.completed",
    "PrefetchCount": 1,
    "AutoAck": false
  },
  "Storage": {
    "BucketName": "optimus-frame-bucket",
    "InputFolder": "bucket_upload",
    "OutputFolder": "processed"
  },
  "AWS": {
    "Region": "us-east-1"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

### Variáveis de Ambiente

```bash
# RabbitMQ
RabbitMQ__HostName=localhost
RabbitMQ__Port=5672
RabbitMQ__UserName=guest
RabbitMQ__Password=guest

# AWS
AWS__Region=us-east-1
AWS_ACCESS_KEY_ID=sua-access-key
AWS_SECRET_ACCESS_KEY=sua-secret-key

# Storage
Storage__BucketName=optimus-frame-bucket
Storage__InputFolder=bucket_upload
Storage__OutputFolder=processed
```

## Como Executar

### Executando Localmente

```bash
# Clonar o repositório
git clone https://github.com/Stack-Food/optimus-frame-transform.git
cd optimus-frame-transform

# Restaurar dependências
dotnet restore

# Executar o Worker
dotnet run --project OptimusFrame.Transform.Worker
```

### Usando Docker

```bash
# Build da imagem Docker
docker build -t optimus-frame-transform:latest -f OptimusFrame.Transform.Worker/Dockerfile .

# Executar container
docker run -d \
  --name optimus-transform-worker \
  -e RabbitMQ__HostName=rabbitmq-host \
  -e AWS__Region=us-east-1 \
  -e AWS_ACCESS_KEY_ID=sua-access-key \
  -e AWS_SECRET_ACCESS_KEY=sua-secret-key \
  -e Storage__BucketName=optimus-frame-bucket \
  optimus-frame-transform:latest
```

### Docker Compose (Desenvolvimento)

```yaml
version: '3.8'

services:
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest

  transform-worker:
    build:
      context: .
      dockerfile: OptimusFrame.Transform.Worker/Dockerfile
    depends_on:
      - rabbitmq
    environment:
      RabbitMQ__HostName: rabbitmq
      AWS__Region: us-east-1
      Storage__BucketName: optimus-frame-bucket
```

## Fluxo de Processamento

### 1. Consumo de Mensagem

```json
{
  "videoId": "123e4567-e89b-12d3-a456-426614174000",
  "fileName": "video.mp4",
  "correlationId": "456e7890-e89b-12d3-a456-426614174111"
}
```

### 2. Pipeline de Processamento

```
1. Worker consome mensagem da fila (video.processing.input)
   ↓
2. Valida mensagem e extrai informações
   ↓
3. Download do vídeo do S3: {bucket}/{inputFolder}/{fileName}
   ↓
4. Extração de frames usando OpenCV
   - Configura FPS, qualidade e formato
   - Extrai frames para diretório temporário
   - Aplica redimensionamento se configurado
   ↓
5. Compressão dos frames em arquivo ZIP
   ↓
6. Upload do ZIP para S3: {bucket}/{outputFolder}/{videoId}_frames.zip
   ↓
7. Limpeza de arquivos temporários
   ↓
8. Publicação de mensagem de conclusão (video.processing.completed)
   ↓
9. ACK da mensagem original
```

### 3. Mensagem de Conclusão Publicada

**Sucesso:**
```json
{
  "videoId": "123e4567-e89b-12d3-a456-426614174000",
  "correlationId": "456e7890-e89b-12d3-a456-426614174111",
  "success": true,
  "framesExtracted": 342,
  "outputUri": "s3://optimus-frame-bucket/processed/123e4567-e89b-12d3-a456-426614174000_frames.zip",
  "processingTimeSeconds": 27.5,
  "errorMessage": null,
  "completedAt": "2024-01-20T10:30:00Z"
}
```

**Falha:**
```json
{
  "videoId": "123e4567-e89b-12d3-a456-426614174000",
  "correlationId": "456e7890-e89b-12d3-a456-426614174111",
  "success": false,
  "framesExtracted": 0,
  "outputUri": null,
  "processingTimeSeconds": 0,
  "errorMessage": "Vídeo não encontrado no S3",
  "completedAt": "2024-01-20T10:30:00Z"
}
```

## Configurações de Extração de Frames

### Opções Predefinidas

O sistema oferece configurações otimizadas através do `FrameExtractionOptions`:

```csharp
// Máxima qualidade (todos os frames, JPEG 95%)
var options = FrameExtractionOptions.HighQuality;

// Otimizado (1 FPS, JPEG 85%)
var options = FrameExtractionOptions.Optimized;

// Baixa qualidade (0.5 FPS, JPEG 70%)
var options = FrameExtractionOptions.LowQuality;
```

### Configuração Customizada

```csharp
var options = new FrameExtractionOptions
{
    TargetFps = 2.0,              // 2 frames por segundo
    Quality = 90,                 // Qualidade JPEG 90%
    Format = ImageFormat.Jpeg,    // Formato de saída
    MaxWidth = 1920,              // Largura máxima
    MaxHeight = 1080,             // Altura máxima
    Scale = 1.0                   // Escala (1.0 = original)
};
```

### Formatos Suportados

- **JPEG**: Melhor compressão, ideal para fotos
- **PNG**: Sem perda, maior tamanho
- **WebP**: Moderna, excelente compressão

## Mensageria RabbitMQ

### Configuração de Filas

```csharp
// Exchange
Exchange: "video.processing"
Type: Direct
Durable: true

// Fila de Entrada
Queue: "video.processing.input"
Routing Key: "video.processar"
Durable: true
AutoDelete: false

// Fila de Saída
Queue: "video.processing.completed"
Durable: true
AutoDelete: false
```

### Estratégia de Confirmação

- **Prefetch Count**: 1 (processa uma mensagem por vez)
- **Auto ACK**: Desabilitado
- **Manual ACK**: Após processamento bem-sucedido
- **NACK + Requeue**: Em caso de cancelamento
- **NACK sem Requeue**: Em caso de erro não recuperável

### Tratamento de Erros

```
Erro Recuperável (ex: timeout temporário)
  → NACK + Requeue = true

Erro Não Recuperável (ex: vídeo não existe)
  → NACK + Requeue = false
  → Publica mensagem de erro na fila completed

Erro Crítico (ex: corrupção de dados)
  → NACK + Requeue = false
  → Log de erro
  → Publica mensagem de erro
```

## Armazenamento AWS S3

### Estrutura de Diretórios

```
s3://optimus-frame-bucket/
├── bucket_upload/              # Vídeos originais
│   ├── usuario1/
│   │   └── mediaId_2024-01-20.mp4
│   └── usuario2/
│       └── mediaId_2024-01-20.mp4
└── processed/                  # Frames processados
    ├── mediaId1_frames.zip
    └── mediaId2_frames.zip
```

### Permissões IAM Necessárias

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "s3:GetObject",
        "s3:PutObject",
        "s3:HeadObject"
      ],
      "Resource": "arn:aws:s3:::optimus-frame-bucket/*"
    }
  ]
}
```

## Monitoramento e Logs

### Níveis de Log

```csharp
// Information: Fluxo normal
"Mensagem recebida - VideoId: {VideoId}"
"Processamento concluído com sucesso! VideoId: {VideoId}, {FrameCount} frames"

// Warning: Situações anormais
"Mensagem inválida recebida: {Message}"
"Processamento cancelado."

// Error: Erros que impedem processamento
"Erro ao deserializar mensagem: {Message}"
"Falha no processamento do VideoId: {VideoId} - {ErrorMessage}"
```

### Métricas Importantes

- Tempo de processamento por vídeo
- Número de frames extraídos
- Taxa de sucesso/falha
- Tamanho dos arquivos ZIP gerados
- Tempo de upload/download S3

### Logs de Exemplo

```
[2024-01-20 10:29:00] INFO: Conectado ao RabbitMQ em localhost:5672
[2024-01-20 10:29:00] INFO: Worker iniciado e aguardando mensagens...
[2024-01-20 10:30:15] INFO: Mensagem recebida - VideoId: 123e4567-e89b-12d3-a456-426614174000
[2024-01-20 10:30:15] INFO: Iniciando processamento do vídeo: 123e4567... - bucket_upload/video.mp4
[2024-01-20 10:30:42] INFO: 342 frames extraídos
[2024-01-20 10:30:45] INFO: Processamento concluído com sucesso! VideoId: 123e4567..., 342 frames em 27.50s
[2024-01-20 10:30:45] INFO: Mensagem de conclusão publicada - VideoId: 123e4567..., Success: True
```

## Performance e Otimização

### Recomendações de Performance

1. **Prefetch Count**: Configure para 1 em ambientes com poucos recursos
2. **FPS Target**: Use 1 FPS para análise geral, 5-10 FPS para detalhes
3. **Qualidade JPEG**: 85% oferece bom equilíbrio entre qualidade e tamanho
4. **Formato de Saída**: JPEG para maioria dos casos, WebP para melhor compressão
5. **Escala**: Reduzir para 0.5 pode diminuir o tempo em 75%

### Tempo Estimado de Processamento

| Duração do Vídeo | FPS | Frames | Tempo Estimado |
|------------------|-----|--------|----------------|
| 1 minuto         | 1   | 60     | ~5-10s         |
| 5 minutos        | 1   | 300    | ~15-30s        |
| 10 minutos       | 1   | 600    | ~30-60s        |
| 30 minutos       | 1   | 1800   | ~1.5-3min      |

## Troubleshooting

### Worker não conecta ao RabbitMQ

**Verificações:**
```bash
# Testar conectividade
telnet localhost 5672

# Verificar logs do RabbitMQ
docker logs rabbitmq-container

# Verificar credenciais
curl -u guest:guest http://localhost:15672/api/overview
```

### Erro ao baixar vídeo do S3

**Causas comuns:**
- Credenciais AWS inválidas ou expiradas
- Bucket não existe ou nome incorreto
- Vídeo não existe no caminho especificado
- Permissões IAM insuficientes

**Solução:**
```bash
# Testar acesso ao S3
aws s3 ls s3://optimus-frame-bucket/bucket_upload/

# Verificar credenciais
aws sts get-caller-identity
```

### Erro de extração de frames

**Causas comuns:**
- Vídeo corrompido ou formato não suportado
- Falta de espaço em disco temporário
- OpenCV não consegue abrir o vídeo

**Solução:**
```bash
# Verificar espaço em disco
df -h /tmp

# Testar vídeo manualmente
ffprobe video.mp4
```

### Mensagem volta para fila constantemente

**Causas comuns:**
- Erro recuperável sendo tratado como NACK + Requeue
- Timeout muito curto
- Recurso externo indisponível

**Solução:**
- Revisar lógica de tratamento de erros
- Implementar Dead Letter Queue (DLQ)
- Aumentar timeout de processamento

## Testes

### Estrutura de Testes (Planejado)

```
tests/
├── OptimusFrame.Transform.UnitTests/
│   ├── UseCases/
│   │   └── ExtractFramesUseCaseTests.cs
│   └── Services/
│       ├── OpenCvFrameExtractionServiceTests.cs
│       └── ZipCompressionServiceTests.cs
└── OptimusFrame.Transform.IntegrationTests/
    ├── S3StorageServiceTests.cs
    └── RabbitMqIntegrationTests.cs
```

### Executar Testes

```bash
# Testes unitários
dotnet test --filter Category=Unit

# Testes de integração
dotnet test --filter Category=Integration

# Com cobertura
dotnet test --collect:"XPlat Code Coverage"
```

## Build e Deploy

### Build Local

```bash
# Compilar solução
dotnet build OptimusFrame.Transform.sln -c Release

# Publicar aplicação
dotnet publish OptimusFrame.Transform.Worker -c Release -o ./publish
```

### Build Docker

```bash
# Build multi-stage
docker build -t optimus-frame-transform:latest \
  -f OptimusFrame.Transform.Worker/Dockerfile .

# Tag para registry
docker tag optimus-frame-transform:latest \
  ghcr.io/stack-food/optimus-frame-transform:latest

# Push para registry
docker push ghcr.io/stack-food/optimus-frame-transform:latest
```

## Integração com Ecossistema OptimusFrame

### Fluxo Completo do Sistema

```
1. OptimusFrame.Auth
   ↓ (usuário autenticado)
2. OptimusFrame.Core (upload vídeo base64)
   ↓ (salva no S3 + publica mensagem RabbitMQ)
3. OptimusFrame.Transform (este serviço)
   ↓ (extrai frames + publica conclusão)
4. OptimusFrame.Core (atualiza status no banco)
   ↓ (notifica usuário)
5. Cliente recebe notificação de conclusão
```

### Endpoints Relacionados

- **POST** `/api/media/upload` (OptimusFrame.Core) → Inicia o fluxo
- **RabbitMQ** `video.processing.input` → Consome vídeos
- **RabbitMQ** `video.processing.completed` → Publica resultados

## Segurança

### Boas Práticas Implementadas

1. **Isolamento de Processos**: Cada vídeo processado em diretório temporário isolado
2. **Limpeza de Recursos**: Finally blocks garantem limpeza de arquivos temporários
3. **Validação de Input**: Validação rigorosa de mensagens antes de processar
4. **Secrets Management**: Credenciais gerenciadas via variáveis de ambiente
5. **IAM Roles**: Suporte para IAM Roles em ambientes AWS

### Recomendações Adicionais

- Use VPC endpoints para comunicação privada com S3
- Configure Dead Letter Queue para mensagens falhadas
- Implemente rate limiting no RabbitMQ
- Use encryption at rest no S3
- Configure CloudWatch Alarms para monitoramento

## Roadmap

- [ ] Implementar testes unitários e de integração completos
- [ ] Adicionar suporte para múltiplos formatos de vídeo (AVI, MOV, etc.)
- [ ] Implementar Dead Letter Queue (DLQ)
- [ ] Adicionar métricas com Prometheus/Grafana
- [ ] Implementar circuit breaker para S3
- [ ] Adicionar suporte para processamento em GPU (CUDA)
- [ ] Implementar cache local de vídeos frequentemente acessados
- [ ] Adicionar análise de conteúdo dos frames (ML)
- [ ] Implementar processamento paralelo de múltiplos vídeos

## Contribuindo

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feature/nova-feature`)
3. Commit suas mudanças (`git commit -m 'Adiciona nova feature'`)
4. Push para a branch (`git push origin feature/nova-feature`)
5. Abra um Pull Request

## Licença

Este projeto está sob a licença MIT. Veja o arquivo LICENSE para mais detalhes.

## Contato

Stack Food Team - team@stackfood.com

Link do Projeto: [https://github.com/Stack-Food/optimus-frame-transform](https://github.com/Stack-Food/optimus-frame-transform)

## Referências

- [OpenCvSharp Documentation](https://github.com/shimat/opencvsharp)
- [OpenCV Documentation](https://docs.opencv.org/)
- [RabbitMQ .NET Client Guide](https://www.rabbitmq.com/dotnet-api-guide.html)
- [AWS S3 SDK for .NET](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/s3-apis-intro.html)
- [Worker Services in .NET](https://docs.microsoft.com/dotnet/core/extensions/workers)
