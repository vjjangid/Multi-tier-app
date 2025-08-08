import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app/app.component';
import { AuthInterceptor } from './app/interceptors/auth.interceptor';

bootstrapApplication(AppComponent, {
  providers: [
    provideHttpClient(withInterceptors([
      (req, next) => {
        // Get the auth token from local storage
        const token = localStorage.getItem('kanban-token');
        
        // Clone the request and add the authorization header if token exists
        if (token) {
          const authReq = req.clone({
            headers: req.headers.set('Authorization', `Bearer ${token}`)
          });
          return next(authReq);
        }
        
        return next(req);
      }
    ]))
  ]
}).catch(err => console.error(err));