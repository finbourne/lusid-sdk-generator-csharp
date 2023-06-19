FROM rust:slim-buster as rust

RUN cargo install just

FROM openapitools/openapi-generator-cli:latest-release as maven

RUN wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh --channel 6.0
RUN apt update && apt -y install jq git gettext-base libicu-dev
ENV DOTNET_ROOT=/root/.dotnet
ENV PATH=$PATH:/root/.dotnet:/root/.dotnet/tools
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
COPY --from=rust /usr/local/cargo/bin/just /usr/bin/just

RUN mkdir -p /usr/src/
WORKDIR /usr/src/

# Make ssh dir
# Create known_hosts
# Add github key
RUN mkdir /root/.ssh/ \
    && touch /root/.ssh/known_hosts \
    && ssh-keyscan github.com >> /root/.ssh/known_hosts

RUN --mount=type=ssh \
    git clone git@github.com:finbourne/lusid-sdk-doc-templates.git /tmp/docs \
    && git clone git@github.com:finbourne/lusid-sdk-workflow-template.git /tmp/workflows

COPY generate/ /usr/src/generate
COPY ./justfile /usr/src/