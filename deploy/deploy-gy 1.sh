#!/bin/bash

# 加载配置
if [ -f "./deploy.config.sh" ]; then
    source ./deploy.config.sh
else
    echo "配置文件 deploy.config.sh 不存在"
    exit 1
fi

echo "开始远程部署 - $(date)"

echo "开始部署您的项目..."

# 1. 克隆项目代码
# echo "1. 克隆项目代码..."
# git clone https://git.zhouxhere.com/wonderzhao/cola-web.git

# 2. 进入项目目录
# echo "2. 进入项目目录..."
# cd cola-web

# 3. 安装依赖
# echo "3. 安装依赖..."
# npm install

# 4. 构建项目
echo "4. 构建项目..."
npm run build

# 5. 构建 Docker 镜像
echo "5. 构建 Docker 镜像..."
docker build -t ${DOCKER_IMAGE_NAME} .

# 6. 保存镜像为 tar 文件
echo "6. 保存 Docker 镜像..."
docker save ${DOCKER_IMAGE_NAME} > ${DOCKER_IMAGE_NAME}.tar

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