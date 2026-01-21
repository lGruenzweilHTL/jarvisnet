import time
import ollama

async def infer_llm(data: dict):
    start = time.perf_counter()

    req_id = data["request_id"]
    text = data["input"]["text"]
    system = data["input"]["system"]
    model = data["config"]["model"]
    temp = data["config"]["temperature"]
    max_tokens = data["config"]["max_tokens"]
    context = data["context"] if "context" in data else ""
    response = prompt_llm(text, system, model, temp, max_tokens, context)

    end = time.perf_counter()
    latency_ms = int((end - start) * 1000)
    result = {
        "request_id": req_id,
        "output": {
            "text": response
        },
        "usage": {
            "latency_ms": latency_ms,
            "model": model
        },
        "error": None
    }
    return result

# TODO: chat functionality
def prompt_llm(text, system, model, temperature, max_tokens, context):
    prompt = "Answer the following question based on the context provided.\n\n"
    if context:
        prompt += f"Context: {context}\n\n"
    prompt += f"Question: {text}\n\n"
    response = ollama.generate(model=model, prompt=prompt, system=system,
                               temperature=temperature, max_tokens=max_tokens)
    return response.response
