FROM alpine

# Install depdencies
RUN apk upgrade --no-cache &&    \
    apk add --no-cache libgcc    \
                       libstdc++ \
                       icu-libs

# Copy application
COPY . /app

# Set working directory
WORKDIR /app

ENTRYPOINT ["./Dibix.Worker.Host"]