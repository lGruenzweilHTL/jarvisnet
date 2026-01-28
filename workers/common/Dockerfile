FROM python:3.11-slim

WORKDIR /opt/assistant
RUN pip install --upgrade pip

# Optimize layer caching so that dependencies are only reinstalled if pyproject.toml changes
COPY ./pyproject.toml ./common/
RUN pip install --no-cache-dir ./common/
COPY ./ ./common/

# Shared directory for Ollama models (mounted from a named volume in compose)
ENV OLLAMA_DIR=/ollama
RUN mkdir -p ${OLLAMA_DIR}

# NOTE: Installing the official Ollama runtime may require steps outside pip
# (e.g., a binary installer). If you want Ollama server installed into the image,
# add the appropriate installation commands here. Alternatively, use the Ollama
# Python client library (pip) which talks to a local Ollama daemon at the OLLAMA_DIR.
