declare var Emby: Emby;
type Emby = {
  Page: {
    currentRouteInfo: {
      path: string;
    };
  };
};

declare var ApiClient: ApiClient;
type ApiClient = {
  getUrl: (url: string, includeAuthorization?: boolean) => Promise<any>;
  getJSON: (url: string, includeAuthorization?: boolean) => Promise<any>;
  fetch: (request: {
    url: string;
    headers?: { [key: string]: string };
    method?: "GET" | "POST" | "PUT" | "DELETE" | "OPTIONS" | "PATCH";
    data?: any;
  }) => Promise<any>;
};
