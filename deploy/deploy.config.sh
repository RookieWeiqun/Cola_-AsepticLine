#!/bin/bash

# 实际部署配置
REMOTE_HOST="192.168.1.199"        # 实际服务器IP,局域网IP
REMOTE_USER="owner"                # 实际用户名
REMOTE_PORT="22"                   # SSH 端口
REMOTE_PATH="/home/owner/dotnet/cola_-aseptic-line2"      # 实际部署路径
DOCKER_IMAGE_NAME="dotnet-app"       # 实际镜像名称
DOCKER_CONTAINER_NAME="dotnet-app:latest" # 实际容器名称
APP_PORT=8080                      # 实际应用端口

# SSH 连接配置
SSH_CONNECT_TIMEOUT=10            # SSH 连接超时时间（秒）
SSH_ALIVE_INTERVAL=60            # 发送保活信号的间隔（秒）
SSH_ALIVE_COUNT=5                # 最大保活次数

# 构建 SSH_OPTS
SSH_OPTS="-o ConnectTimeout=${SSH_CONNECT_TIMEOUT} \
          -o ServerAliveInterval=${SSH_ALIVE_INTERVAL} \
          -o ServerAliveCountMax=${SSH_ALIVE_COUNT}"

# 建议添加的配置
DOCKER_IMAGE_TAG="latest"         # 添加镜像标签
BACKUP_PATH="/home/owner/backups"  # 添加备份路径
MAX_BACKUP_COUNT=5                # 保留的备份数量 