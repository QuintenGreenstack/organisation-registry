import { FormBuilder, FormGroup } from '@angular/forms';
import { ActivatedRoute, Router, Params } from '@angular/router';
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

import * as moment from 'moment/moment';

import { AlertService, AlertBuilder } from 'core/alert';

import {OrganisationSyncService, OrganisationTermination} from 'services/organisationsync';
import {Observable} from "rxjs/Observable";
import {OrganisationInfoService} from "../../../services";
import {Subscription} from "rxjs/Subscription";
import {Organisation, OrganisationService} from "../../../services/organisations";
import {OidcService, Role} from "../../../core/auth";
import {BaseAlertMessages} from "../../../core/alertmessages";
import {required} from "../../../core/validation";

@Component({
  templateUrl: 'terminate.template.html',
  styleUrls: ['terminate.style.css'],
})
export class OrganisationTerminateComponent implements OnInit{
  @Output('onCheckkboNumber') onCheckkboNumber: EventEmitter<string> = new EventEmitter<string>();

  public isLoading: boolean = true;
  public canEditOrganisation: Observable<boolean>;
  public terminateForm: FormGroup;
  public today: string;
  public organisation: Organisation;
  public organisationTermination: OrganisationTermination;

  private readonly alertMessages: BaseAlertMessages = new BaseAlertMessages('Organisatie stopzetten');
  private readonly subscriptions: Subscription[] = new Array<Subscription>();
  private organisationId: string;


  constructor(
    formBuilder: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private organisationSyncService: OrganisationSyncService,
    private organisationService: OrganisationService,
    private store: OrganisationInfoService,
    private oidcService: OidcService,
    private alertService: AlertService  ) {

    this.today = moment().format('YYYY-MM-DD');

    this.terminateForm = formBuilder.group({
      terminationDate: ['', required],
    });
  }

  @Input('isBusy')
  public set isBusy(isBusy: boolean) {
    if (isBusy) {
      this.terminateForm.disable();
    } else {
      this.terminateForm.enable();
    }
  }

  ngOnInit() {
    let organisationChangedObservable =
      this.store.organisationChanged;

    this.subscriptions.push(organisationChangedObservable
      .subscribe(organisation => {
        if (organisation) {
          this.organisation = organisation;
          this.loadSyncStatus();
        }
      }));

    this.canEditOrganisation = Observable.of(false);
    this.subscriptions.push(this.route.parent.parent.params.subscribe((params: Params) => {
      this.organisationId = params['id'];
      this.canEditOrganisation = this.oidcService.hasAnyOfRoles([Role.OrganisationRegistryBeheerder]);
      this.store.loadOrganisation(this.organisationId);
    }));
  }

  submit() {
    this.organisationService.terminate(this.organisationId, this.terminateForm.get('terminationDate').value)
      .finally(() => {
        this.isBusy = false;
      })
      .subscribe(
        result => {
          this.router.navigate(['./..'], { relativeTo: this.route });

          this.alertService.setAlert(
            new AlertBuilder()
              .success()
              .withTitle('Organisatie bijgewerkt!')
              .withMessage('De koppeling met de KBO werd succesvol ongedaan gemaakt.')
              .build());
        },
        error => {
          this.router.navigate(['./..'], { relativeTo: this.route });
          this.alertService.setAlert(
            new AlertBuilder()
              .error(error)
              .build());
        });
  }

  private loadSyncStatus() {
    this.isLoading = true;

    this.organisationSyncService.get(this.organisationId, this.organisation.kboNumber)
      .finally(() => this.isLoading = false)
      .subscribe(
        item => {
          if (item){
            this.organisationTermination = item;
            this.terminateForm.setValue({terminationDate: item.date || null})
          }
        },
        error => this.alertService.setAlert(
          new AlertBuilder()
            .error(error)
            .withTitle(this.alertMessages.loadError.title)
            .withMessage(this.alertMessages.loadError.message)
            .build()));
  }
}
