namespace AssistantCore.Workers;

public interface ILlmWorkerFactory
{
    ILlmWorker GetWorkerBySpeciality(LlmSpeciality speciality);
}