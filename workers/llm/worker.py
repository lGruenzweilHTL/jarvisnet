from common.runtime import WorkerRuntime
from app import infer_llm

runtime = WorkerRuntime(
    worker_type="llm:home-control",
    infer_handler=infer_llm,
)

runtime.run()
