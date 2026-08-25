// Empty base URL = same-origin: production deployments serve the API from the same host as the
// SPA (behind a reverse proxy), so no absolute URL is needed.
export const environment = {
  production: true,
  apiBaseUrl: '',
  notificationsHubUrl: '/hubs/notifications',
};
