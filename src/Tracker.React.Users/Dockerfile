FROM node:24.8.0-alpine3.22

WORKDIR /app

# First dependencies (accelerates cache)
COPY package*.json ./
RUN npm install

# Source files
COPY . .

EXPOSE 5000

# For container is nessesary host=0.0.0.0
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]