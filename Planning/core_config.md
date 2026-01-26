# Core Configuration Reference

## Environment Variables

- USE_SEQ: Whether to write logs to a seq server. Default is false.
- SEQ_URL: The URL of the seq server. Default is "http://localhost:5341".
- USE_FILE_LOGGING: Whether to write logs to a file. Default is false.
- LOG_DIRECTORY: The directory where log files will be stored. Default is "./logs". Logs are named using the pattern "assistantcore-{Date}.json".
- MINIMUM_LOG_LEVEL: The minimum log level for logging. Default is "Information". Possible values are "Verbose", "Debug", "Information", "Warning", "Error" and "Fatal".