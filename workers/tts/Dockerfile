FROM workers-common
ENV PORT=8000

WORKDIR /opt/assistant

RUN pip install --upgrade pip

COPY ./ ./tts/
RUN pip install --no-cache-dir tts/

EXPOSE $PORT

CMD ["python", "-m", "tts.worker"]
