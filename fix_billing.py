import re

with open("Controllers/AdminBillingController.cs", "r") as f:
    lines = f.readlines()

new_lines = []
for i, line in enumerate(lines):
    if line.strip() == '[Authorize(Roles = "admin,super_admin")]':
        new_lines.append(line.replace('"admin,super_admin"', '"admin,super_admin,sub_super_admin"'))
    elif line.strip().startswith("public async Task<IActionResult>"):
        method_name = line.strip().split("Task<IActionResult> ")[1].split("(")[0]
        permission = ""
        if "PlanStats" in method_name or "PlansAll" in method_name or "PlanDetail" in method_name or "PlanStudents" in method_name:
            permission = "Membership.View"
        elif "PlansCreate" in method_name or "PlanUpdate" in method_name or "PlanToggle" in method_name or "PlanDelete" in method_name:
            permission = "Membership.ManagePlans"
        elif "MembershipsExpiring" in method_name or "MembershipsExpiredToday" in method_name or "MembershipsList" in method_name or "MembershipDetail" in method_name:
            permission = "Membership.View"
        elif "MembershipsAssign" in method_name or "MembershipsRenew" in method_name or "MembershipsUpgrade" in method_name:
            permission = "Membership.ManagePlans"
        elif "PaymentsSummary" in method_name or "PaymentsPending" in method_name or "PaymentsOverdue" in method_name or "PaymentsList" in method_name or "PaymentDetail" in method_name or "PaymentReceipt" in method_name or "SendPaymentReceipt" in method_name:
            permission = "Payment.View"
        elif "PaymentsCreate" in method_name or "PaymentVerify" in method_name or "PaymentUpdate" in method_name:
            permission = "Payment.Verify"
        elif "PaymentRefund" in method_name:
            permission = "Payment.Refund"
        
        if permission:
            indent = line[:len(line) - len(line.lstrip())]
            new_lines.append(f'{indent}[AuthorizePermission(Permissions.{permission})]\n')
        new_lines.append(line)
    elif "namespace WebApplication1.Controllers" in line:
        new_lines.append("using WebApplication1.Utils;\n\n")
        new_lines.append(line)
    else:
        new_lines.append(line)

with open("Controllers/AdminBillingController.cs", "w") as f:
    f.writelines(new_lines)
