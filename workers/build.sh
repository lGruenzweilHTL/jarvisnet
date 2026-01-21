docker build ./common -t workers-common
docker build ./stt -t worker-stt
docker build ./router -t worker-router
#docker build ./llm -t worker-llm
docker build ./tts -t worker-tts