FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:6.0

RUN apt-get update && apt-get install -y jq

RUN mkdir -p /usr/src/
RUN mkdir -p /usr/work/
WORKDIR /usr/src/
