# bunqJob

A minimal .net application to automatically transfer money to other accounts.

## Configuration

Update the appsettings.json accoring to your requirments.

## Docker

For the docker implementation the following commands are needed:

```bash
docker pull duijvelshoff/bunqjob:latest
docker run --name bunqjob duijvelshoff/bunqjob:latest
docker cp appsettings.json bunqjob:/app/appsettings.json
docker run bunqjob && docker logs bunqjob
```

*NOTE: You can igonre the error on first boot.

Last but not least, add to crontab:
```bash
0 0 1 * *  docker run bunqjob
```

## Installation

First you publish the application and copy the contents to a preferred folder (linux based):

```bash
dotnet publish --configuration Release --framework netcoreapp2.1
mkdir -p /usr/local/bin/bunqJob
cp bin/Release/netcoreapp2.1/publish/* /usr/local/bin/bunqJob
```

Then for the first boot you run and add your API key:
```bash
cd /usr/local/bin/bunqJob && dotnet bunqJob.dll
```

Last but not least, add to crontab:
```bash
0 0 1 * *  cd /usr/local/bin/bunqJob && dotnet bunqJob.dll >> /usr/local/bin/bunqJob/bunqJob.log
```

_If needed install .net core 2.1 and point to the binary._
