FROM workers-common
ENV PORT=8000

WORKDIR /opt/assistant

RUN pip install --upgrade pip

COPY ./ ./stt/
RUN pip install --no-cache-dir stt/

EXPOSE $PORT

CMD ["python", "-m", "stt.worker"]
