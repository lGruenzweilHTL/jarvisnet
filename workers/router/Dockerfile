FROM workers-common
ENV PORT=8000

WORKDIR /opt/assistant

RUN pip install --upgrade pip

# Optimize layer caching so that dependencies are only reinstalled if pyproject.toml changes
COPY ./pyproject.toml ./router/
RUN pip install --no-cache-dir ./router/
COPY ./ ./router/

EXPOSE $PORT

CMD ["python", "-m", "router.worker"]
