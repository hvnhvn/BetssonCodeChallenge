# BEFORE YOU START REVIEWING:

This repository contains Unit and Integration tests for the provided microservice.

Although the task required that all tests pass successfully, some tests are intentionally designed to fail. These tests highlight specific cases of system behavior that I, as a QA engineer, consider problematic or potentially incorrect.

Each failing test includes a comment explaining the reasoning behind the failure and the issue it is intended to demonstrate.

# qa-backend-code-challenge

Code challenge for QA Backend Engineer candidates.

### Build Docker image

Run this command from the directory where there is the solution file.

```
docker build -f src/Betsson.OnlineWallets.Web/Dockerfile .
```

### Run Docker container

```
docker run -p <port>:8080 <image id>
```

### Open Swagger

```
http://localhost:<port>/swagger/index.html
```
