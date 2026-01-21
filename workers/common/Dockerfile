# ---- Build stage ----
FROM python:3.11-slim AS builder
ENV PORT=8000
ENV CORE_URL=http://localhost:3000

WORKDIR /opt/assistant

COPY ./ ./common/
RUN pip install --upgrade pip
RUN pip install --no-cache-dir ./common/