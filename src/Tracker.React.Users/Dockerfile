FROM node:24.8.0-alpine3.22

WORKDIR /app

# First dependencies (accelerates cache)
COPY package*.json ./
RUN npm install --ignore-scripts

#USER node
#RUN npm ci --ignore-scripts

# Source files
# Give access to root and then changing the owner to supress Sonar warnings
COPY --chown=root:root --chmod=755 public ./public
RUN chown -R node:node ./public && \
    chmod -R u=rwX,go=rX ./public

COPY --chown=root:root src ./src
RUN chown -R node:node ./src && \
    chmod -R u=rwX,go=rX ./src

COPY --chown=root:root tsconfig*.json ./
COPY --chown=root:root vite.config.ts ./
RUN chown -R node:node tsconfig*.json vite.config.ts
RUN chmod u=rw,go=r tsconfig*.json vite.config.ts

# only for dev environment, it is better to pass secrets through command line or .env files of docker compose
COPY --chown=root:root .env.development ./
RUN chown -R node:node .env.development && \
    chmod -R u=rw,go=r .env.development

USER node

EXPOSE 5000

# For container is nessesary host=0.0.0.0
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]