# BEFORE YOU START REVIEWING:

This repository contains Unit, Integration and System tests for the provided microservice.

For demonstration purposes, most API tests are provided in both Integration and System versions.

Although the task required that all tests pass successfully, some tests are intentionally designed to fail. These tests highlight specific cases of system behavior that I, as a QA engineer, consider problematic or potentially incorrect.

Each failing test includes a comment explaining the reasoning behind the failure and the issue it is intended to demonstrate.

### Running System tests

To run the system tests, you need to start a Docker container with the ```-p 8080:8080``` option.

Alternatively, you can run the ```run-system-tests.ps1``` script from the root folder. This script will build and start the Docker container, execute the system tests, and then clean up the container automatically.

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
