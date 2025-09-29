FROM node:24.8.0-alpine3.22

WORKDIR /app

# First dependencies (accelerates cache)
COPY package*.json ./
RUN npm install --ignore-scripts

USER node
RUN npm ci

# Source files
COPY --chown=node:node public ./public
COPY --chown=node:node src ./src
COPY --chown=node:node tsconfig*.json ./
COPY --chown=node:node vite.config.* ./
COPY --chown=node:node .eslint* ./

# only for dev environment, it is better to pass secrets through command line or .env files of docker compose
COPY --chown=node:node .env* ./

EXPOSE 5000

# For container is nessesary host=0.0.0.0
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]