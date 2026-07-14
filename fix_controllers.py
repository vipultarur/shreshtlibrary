import os
import re

controllers_dir = "Controllers"
files = [
    "AdminSeatsController.cs",
    "AdminLibraryController.cs",
    "AdminNotificationsController.cs",
    "AdminReviewsController.cs",
    "AdminSettingsController.cs",
    "AdminSlidersController.cs"
]

for filename in files:
    filepath = os.path.join(controllers_dir, filename)
    with open(filepath, "r", encoding="utf-8") as f:
        lines = f.readlines()

    new_lines = []
    has_using_utils = False
    for line in lines:
        if "using WebApplication1.Utils;" in line:
            has_using_utils = True

    for line in lines:
        if "namespace WebApplication1.Controllers" in line and not has_using_utils:
            new_lines.append("using WebApplication1.Utils;\n\n")
            new_lines.append(line)
        elif line.strip() == '[Authorize(Roles = "admin,super_admin")]':
            new_lines.append(line.replace('"admin,super_admin"', '"admin,super_admin,sub_super_admin"'))
        elif line.strip().startswith("public async Task<IActionResult>") or line.strip().startswith("public IActionResult"):
            method_name = line.strip().split("IActionResult> ")[1].split("(")[0] if "Task<" in line else line.strip().split("IActionResult ")[1].split("(")[0]
            permission = ""
            
            # Seats
            if filename == "AdminSeatsController.cs":
                if "SeatsLayout" in method_name or "SeatsAvailable" in method_name or "SeatsStats" in method_name or "SeatsList" in method_name or "SeatDetail" in method_name or "SeatHistory" in method_name or "FloorsList" in method_name or "FloorDetail" in method_name or "RowsList" in method_name or "RowDetail" in method_name:
                    permission = "LibraryManagement.Seat"
                elif "SeatsReleaseAll" in method_name or "SeatsReserveBulk" in method_name or "SeatsAdd" in method_name or "SeatDelete" in method_name or "SeatUpdate" in method_name or "SeatStatus" in method_name or "SeatAssign" in method_name or "SeatUnassign" in method_name or "FloorAdd" in method_name or "FloorDelete" in method_name or "RowAdd" in method_name or "RowDelete" in method_name or "FloorUpdate" in method_name or "RowUpdate" in method_name:
                    permission = "LibraryManagement.Seat"
            
            # Library
            elif filename == "AdminLibraryController.cs":
                if "Info" in method_name or "Facility" in method_name or "Facilities" in method_name or "UpdateInfo" in method_name or "UpdateWelcome" in method_name:
                    permission = "LibraryManagement.Info"
                elif "Gallery" in method_name or "Image" in method_name:
                    permission = "LibraryManagement.Gallery"
                if "Facility" in method_name or "Facilities" in method_name:
                    permission = "LibraryManagement.Facilities"

            # Notifications
            elif filename == "AdminNotificationsController.cs":
                if "List" in method_name or "Detail" in method_name:
                    permission = "NotificationManagement.View"
                else:
                    permission = "NotificationManagement.Send"
            
            # Reviews
            elif filename == "AdminReviewsController.cs":
                permission = "LibraryManagement.Review"
                
            # Settings
            elif filename == "AdminSettingsController.cs":
                permission = "AppSettings.Manage"
                
            # Sliders
            elif filename == "AdminSlidersController.cs":
                permission = "LibraryManagement.Slider"
                
            if permission:
                indent = line[:len(line) - len(line.lstrip())]
                new_lines.append(f'{indent}[AuthorizePermission(Permissions.{permission})]\n')
            new_lines.append(line)
        else:
            new_lines.append(line)

    with open(filepath, "w", encoding="utf-8") as f:
        f.writelines(new_lines)
