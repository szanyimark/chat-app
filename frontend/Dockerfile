# Development stage (used by docker-compose for dev)
FROM node:20-alpine AS development
WORKDIR /app
RUN npm install -g @angular/cli@17
COPY package.json ./
COPY package-lock.json ./
RUN npm install
COPY . .
EXPOSE 4200
CMD ["ng", "serve", "--host", "0.0.0.0"]

# Build stage (for production builds)
FROM development AS build
RUN npm run build

# Production stage (nginx serving static files)
FROM nginx:alpine AS production
COPY --from=build /app/dist/chat-app/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]