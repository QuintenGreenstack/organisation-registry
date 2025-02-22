import { Injectable } from '@angular/core';
import {
  ActivatedRouteSnapshot,
  CanActivate,
  CanActivateChild,
  RouterStateSnapshot
} from '@angular/router';

import { Observable } from 'rxjs/Observable';
import {OidcService} from "core/auth";
import {OrganisationInfoService} from "../../services";

@Injectable()
export class OrganisationGuard implements CanActivate, CanActivateChild {
  constructor(
    private oidcService: OidcService,
    private organisationStore: OrganisationInfoService
  ) {
  }

  public canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> {
    return this.checkPermissions(route);
  }

  public canActivateChild(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> {
    return this.checkPermissions(route);
  }

  private checkPermissions(route): Observable<boolean> {
    let organisationIdPart = route.data['organisationGuard'].idPart as string;
    let organisationParams = route.data['organisationGuard'].params as string;
    let params;
    eval('params = ' + organisationParams + ';');

    this.organisationStore.loadOrganisation(params[organisationIdPart]);

    return this.organisationStore.organisationChanged.flatMap(o => {
      return this.organisationStore.canUpdateOrganisationChanged$

    }).map(allowed => {
      if (allowed)
        return true;

      this.redirectToAuth();
      return false;
    });
  }

  private redirectToAuth() {
    this.oidcService.signIn();
  }
}
