from common.runtime import WorkerRuntime
from stt.app import infer_stt

runtime = WorkerRuntime(
    worker_type="tts",
    infer_handler=infer_stt,
)

runtime.run()
