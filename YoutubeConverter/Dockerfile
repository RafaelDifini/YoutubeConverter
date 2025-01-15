# Usar uma imagem base com .NET Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Instalar dependências: ffmpeg, Python e criar ambiente virtual
RUN apt-get update && apt-get install -y ffmpeg python3-pip python3-venv

# Criar e ativar o ambiente virtual para instalar yt-dlp
RUN python3 -m venv /venv
RUN /venv/bin/pip install yt-dlp

# Configurar o PATH para usar o ambiente virtual
ENV PATH="/venv/bin:$PATH"

# Copiar os arquivos publicados para o container
COPY out . 

# Copiar o certificado para o container
COPY https-cert.pfx /https-cert.pfx

# Configurar a variável de ambiente para o certificado
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/https-cert.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=senha

# Expor as portas HTTP e HTTPS especificadas no launchSettings.json
EXPOSE 5173
EXPOSE 7259

# Executar a aplicação
ENTRYPOINT ["dotnet", "YoutubeConverter.dll"]
