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
    docker build -t ${DOCKER_IMAGE_NAME}:${DOCKER_IMAGE_TAG} .
    
    # 返回上级目录执行 docker-compose
    cd ../../
    docker-compose down
    docker-compose up -d

    # 清理不再使用的镜像
    docker image prune -f
EOF









# 7. 传输文件到服务器
echo "7. 传输文件到服务器..."
# ssh -p 22 owner@192.168.1.199
scp -v -P 22 cola-web.tar owner@192.168.1.199:web/
scp -v -P ${REMOTE_PORT} ${DOCKER_IMAGE_NAME}.tar ${REMOTE_USER}@${REMOTE_HOST}:${REMOTE_PATH}/

# 8. 在远程服务器上执行部署
echo "8. 在远程服务器上执行部署..."
ssh -p ${REMOTE_PORT} ${REMOTE_USER}@${REMOTE_HOST} << EOF
    set -e  # 遇到错误立即退出

    echo "===== 开始执行远程部署 ====="
    cd ${REMOTE_PATH}
    docker load < ${DOCKER_IMAGE_NAME}.tar
    docker stop ${DOCKER_CONTAINER_NAME} 2>/dev/null || true
    docker rm ${DOCKER_CONTAINER_NAME} 2>/dev/null || true
    docker run -d \
        --name ${DOCKER_CONTAINER_NAME} \
        -p ${APP_PORT}:80 \
        --restart unless-stopped \
        ${DOCKER_IMAGE_NAME}
EOF

echo "部署完成！"
# echo "部署完成！"

# 10. 检查部署状态
# echo "10. 检查部署状态..."
# ssh owner@192.168.1.199 "docker ps | grep cola-web"

# 在另一个终端窗口查看日志
# tail -f deploy.log