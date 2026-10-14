import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';

function getWaitingRoomHubUrl() {
  const apiBaseUrl = import.meta.env.VITE_API_URL
    || (import.meta.env.DEV ? 'http://localhost:5080/api' : '/api');
  const apiUrl = new URL(apiBaseUrl, window.location.origin);
  const basePath = apiUrl.pathname.replace(/\/api\/?$/i, '').replace(/\/$/, '');
  return new URL(`${basePath}/hubs/waiting-room`, apiUrl.origin).toString();
}

export function createWaitingRoomConnection(getAccessToken) {
  return new HubConnectionBuilder()
    .withUrl(getWaitingRoomHubUrl(), {
      accessTokenFactory: getAccessToken
    })
    .withAutomaticReconnect()
    .configureLogging(import.meta.env.DEV ? LogLevel.Warning : LogLevel.Error)
    .build();
}

export { HubConnectionState };
