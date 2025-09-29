FROM node:24.8.0-alpine3.22

WORKDIR /app

# First dependencies (accelerates cache)
COPY package*.json ./

COPY package*.json ./
RUN npm install

# Source files
COPY public ./public
COPY src ./src
COPY tsconfig*.json ./
COPY vite.config.ts ./

# only for dev environment, it is better to pass secrets through command line or .env files of docker compose
COPY .env.development ./

EXPOSE 5000

# For container is nessesary host=0.0.0.0
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]