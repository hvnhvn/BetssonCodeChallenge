# Build the Docker image
docker build -f src/Betsson.OnlineWallets.Web/Dockerfile -t onlinewalletservice .

# Run container
docker run -d -p 8080:8080 --name onlinewalletservice-container onlinewalletservice

# Run tests
dotnet test tests\Betsson.OnlineWallets.SystemTests
$TEST_RESULT = $LASTEXITCODE

# Stop and remove container
docker stop onlinewalletservice-container
docker rm onlinewalletservice-container

# Exit with test result code
exit $TEST_RESULT