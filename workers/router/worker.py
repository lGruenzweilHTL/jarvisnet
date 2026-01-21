from common.runtime import WorkerRuntime
from app import infer_router

runtime = WorkerRuntime(
    worker_type="tts",
    infer_handler=infer_router,
)

runtime.run()
