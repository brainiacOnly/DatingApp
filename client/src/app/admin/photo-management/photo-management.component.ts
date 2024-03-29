import { Component, OnInit } from '@angular/core';
import {Photo} from "../../models/photo";
import {AdminService} from "../../services/admin.service";

@Component({
  selector: 'app-photo-management',
  templateUrl: './photo-management.component.html',
  styleUrls: ['./photo-management.component.css']
})
export class PhotoManagementComponent implements OnInit {
  photos: Photo[] = [];

  constructor(private adminService: AdminService) { }

  ngOnInit(): void {
    this.getPhotosForApproval();
  }

  getPhotosForApproval() {
    this.adminService.getPhotosForApproval().subscribe(photos => this.photos = photos);
  }

  approvePhoto(id: number) {
    this.adminService.approvePhoto(id).subscribe(() => this.photos.splice(this.photos.findIndex(p => p.id === id), 1));
  }

  rejectPhoto(id: number) {
    this.adminService.rejectPhoto(id).subscribe(() => this.photos.splice(this.photos.findIndex(p => p.id === id), 1));
  }

}
