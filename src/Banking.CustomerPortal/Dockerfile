FROM node:24-alpine AS build
WORKDIR /app

COPY src/Banking.CustomerPortal/package.json src/Banking.CustomerPortal/package-lock.json ./
RUN npm install

COPY src/Banking.CustomerPortal/ ./
RUN npm run build

FROM nginx:1.29-alpine AS runtime
COPY src/Banking.CustomerPortal/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
