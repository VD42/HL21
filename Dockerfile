FROM ubuntu
RUN apt-get update && apt-get install -y libcurl4
WORKDIR /app
COPY bin/x64/Release/HL21++.out ./
ENTRYPOINT ["/app/HL21++.out"]