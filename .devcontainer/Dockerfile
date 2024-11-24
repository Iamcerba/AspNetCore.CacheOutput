ARG VARIANT="8.0"

FROM --platform="linux/arm64" mcr.microsoft.com/devcontainers/dotnet:1-${VARIANT}

RUN apt-get -y update && \
    apt-get install --no-install-recommends -y apt-transport-https ca-certificates curl && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Install OpenSSL 1.1 (.NET Core 3.1 dependency)
RUN wget http://ports.ubuntu.com/pool/main/o/openssl/libssl1.1_1.1.1f-1ubuntu2_arm64.deb && \
    dpkg -i libssl1.1_1.1.1f-1ubuntu2_arm64.deb
