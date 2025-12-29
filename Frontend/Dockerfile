FROM node:20-alpine AS build

WORKDIR /src
COPY package*.json ./
RUN npm ci --legacy-peer-deps
COPY . .
RUN npm run build -- --configuration production
FROM nginx:alpine

COPY --from=build /dist/taskflow /usr/share/nginx/html  

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]