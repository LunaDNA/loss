FROM alpine:latest
LABEL maintainer="ci@lunadna.com"

RUN mkdir /root/.aws && \
    echo -e "[default]\noutput = json\nregion = us-west-1" >> /root/.aws/config

RUN apk --update add py-pip && \
    pip install --upgrade awscli && \
    rm /var/cache/apk/*

ENTRYPOINT ["aws"]
