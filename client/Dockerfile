FROM node:22-alpine AS build
WORKDIR /app

# The package files first, so a code change doesn't install the packages again
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

# The final image has only nginx and the built shop
FROM nginx:1.29-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
