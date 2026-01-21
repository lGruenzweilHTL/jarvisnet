FROM workers-common
ENV PORT=8000

WORKDIR /opt/assistant

RUN pip install --upgrade pip

COPY ./ ./llm/
RUN pip install --no-cache-dir llm/

EXPOSE $PORT

CMD ["python", "-m", "llm.worker"]
