FROM ubuntu
RUN apt-get update && apt-get install -y g++ make
WORKDIR /app
COPY curl-7.75.0 /app/curl-7.75.0
RUN /app/curl-7.75.0/configure --disable-dependency-tracking && make --directory=/app/curl-7.75.0 && make --directory=/app/curl-7.75.0 install && ldconfig
COPY rapidjson /app/rapidjson
COPY date.h /app/date.h
COPY main.cpp /app/main.cpp
RUN g++ -c -x c++ /app/main.cpp -g1 -o "/app/main.o" -Wall -Wswitch -W"no-deprecated-declarations" -W"empty-body" -Wconversion -W"return-type" -Wparentheses -W"no-format" -Wuninitialized -W"unreachable-code" -W"unused-function" -W"unused-value" -W"unused-variable" -O3 -fno-strict-aliasing -fomit-frame-pointer -DNDEBUG -fthreadsafe-statics -fexceptions -frtti -std=c++17 && g++ -o "/app/HL21++.out" -Wl,--no-undefined -Wl,-z,relro -Wl,-z,now -Wl,-z,noexecstack /app/main.o -lcurl -lpthread
ENTRYPOINT ["/app/HL21++.out"]