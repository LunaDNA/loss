# aws-cli
[AWS Command Line Interface](https://aws.amazon.com/cli/) based on Linux Alpine in Docker

DockeHub [lunadna/aws-cli](https://hub.docker.com/r/lunadna/aws-cli/)

## Build
Built automatically via DockerCloud

[![Badge](http://dockeri.co/image/lunadna/aws-cli)](https://hub.docker.com/r/lunadna/aws-cli/)


## Usage

Will need AWS credentials via:

* Mounting `.aws/` (after `aws configure`) from local machine to docker container
* Setting container environment variables with AWS access keys
* Assuming an IAM role with adequate permissions (ie. on an EC2 instance)

The default AWS region is `us-west-1`

### Examples

```
docker run --rm \
	lunadna/aws-cli \
	ec2 describe-instances
docker run --rm \
	-v $HOME/.aws:/root/.aws \
	-v $(pwd):/s3-bucket \
	lunadna/aws-cli \
	s3 sync s3://<bucket-name> s3-bucket
docker run --rm \
	-e "AWS_ACCESS_KEY_ID=<access key>" \
	-e "AWS_SECRET_ACCESS_KEY=<secret access key>" \
	lunadna/aws-cli \
	ecr describe-repositories
docker run --rm -it \
	--entrypoint /bin/sh \
	lunadna/aws-cli

```

## License

Copyright 2019 LunaPBC

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.

You may obtain a copy of the License at [http://www.apache.org/licenses/LICENSE-2.0](http://www.apache.org/licenses/LICENSE-2.0)

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
