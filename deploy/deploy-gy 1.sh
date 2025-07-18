#!/bin/bash

# 加载配置
if [ -f "./deploy.config.sh" ]; then
    source ./deploy.config.sh
else
    echo "配置文件 deploy.config.sh 不存在"
    exit 1
fi

echo "开始远程部署 - $(date)"

echo "开始部署.NET后端项目..."


# 1. 传输文件到服务器
echo "2. 传输文件到服务器..."
# 传输项目文件和 Dockerfile
scp -r -P ${REMOTE_PORT} ../../Cola/* ${REMOTE_USER}@${REMOTE_HOST}:${REMOTE_PATH}/

# 2. 在远程服务器上执行部署
echo "3. 在远程服务器上执行部署..."
ssh -p ${REMOTE_PORT} ${REMOTE_USER}@${REMOTE_HOST} << EOF
    set -e  # 遇到错误立即退出

    echo "===== 开始执行远程部署 ====="
    
    # 进入项目目录构建镜像
    cd ${REMOTE_PATH}
    echo "正在构建Docker镜像: ${DOCKER_IMAGE_NAME}:${DOCKER_IMAGE_TAG}"
    docker build -t ${DOCKER_IMAGE_NAME}:${DOCKER_IMAGE_TAG} .
    
    # 停止并删除旧容器（如果存在）
    echo "停止并移除旧容器（如果存在）..."
    docker stop docker-cola 2>/dev/null || true
    docker rm docker-cola 2>/dev/null || true
    
    # 启动新容器
    echo "启动新容器..."
    docker run -d -p 8082:8080 \
	  -e TZ=Asia/Shanghai \
      -e ASPNETCORE_ENVIRONMENT=Development \
      --restart unless-stopped \
      --name docker-cola \
      ${DOCKER_IMAGE_NAME}:${DOCKER_IMAGE_TAG}
    
    # 删除未命名镜像（dangling images）
    echo "清理未使用的镜像..."
    docker image prune -f
    
    echo "部署完成！应用程序正在运行: http://${REMOTE_HOST}:8082"
EOF

echo "远程部署完成 - $(date)"