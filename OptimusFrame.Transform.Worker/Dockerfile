# Acesse https://aka.ms/customizecontainer para saber como personalizar seu contêiner de depuração e como o Visual Studio usa este Dockerfile para criar suas imagens para uma depuração mais rápida.

# Esta fase é usada durante a execução no VS no modo rápido (Padrão para a configuração de Depuração)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
# Dependências de sistema exigidas pelo OpenCvSharpExtern.so e pelo FFMpegCore no Linux
RUN apt-get update && apt-get install -y --no-install-recommends \
    # OpenCV: suporte a janelas e paralelismo
    libglib2.0-0 \
    libgl1 \
    libgomp1 \
    libsm6 \
    libxrender1 \
    libxext6 \
    # FFmpeg: necessário para OpenCV ler vídeos e para FFMpegCore
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app


# Esta fase é usada para compilar o projeto de serviço
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Copia todos os .csproj antes do restore para que o NuGet resolva
# dependências transitivas (incluindo OpenCvSharp4.runtime.linux) corretamente
COPY ["OptimusFrame.Transform.Worker/OptimusFrame.Transform.Worker.csproj", "OptimusFrame.Transform.Worker/"]
COPY ["OptimusFrame.Transform.Application/OptimusFrame.Transform.Application.csproj", "OptimusFrame.Transform.Application/"]
COPY ["OptimusFrame.Transform.Infrastructure/OptimusFrame.Transform.Infrastructure.csproj", "OptimusFrame.Transform.Infrastructure/"]
COPY ["OptimusFrame.Transform.Domain/OptimusFrame.Transform.Domain.csproj", "OptimusFrame.Transform.Domain/"]
RUN dotnet restore "./OptimusFrame.Transform.Worker/OptimusFrame.Transform.Worker.csproj"
COPY . .
WORKDIR "/src/OptimusFrame.Transform.Worker"
RUN dotnet build "./OptimusFrame.Transform.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase é usada para publicar o projeto de serviço a ser copiado para a fase final
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OptimusFrame.Transform.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase é usada na produção ou quando executada no VS no modo normal (padrão quando não está usando a configuração de Depuração)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OptimusFrame.Transform.Worker.dll"]