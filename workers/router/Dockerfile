FROM workers-common
ENV PORT=8000

WORKDIR /opt/assistant

RUN pip install --upgrade pip

COPY ./ ./router/
RUN pip install --no-cache-dir router/

EXPOSE $PORT

CMD ["python", "-m", "router.worker"]
