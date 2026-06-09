from rest_framework.response import Response
from rest_framework.views import exception_handler
from rest_framework import status

def standard_response(status_str="success", message="", data=None, errors=None, status_code=status.HTTP_200_OK):
    res_data = {
        "status": status_str,
        "message": message,
    }
    if data is not None:
        res_data["data"] = data
    if errors is not None:
        res_data["errors"] = errors
    return Response(res_data, status=status_code)

def custom_exception_handler(exc, context):
    response = exception_handler(exc, context)

    if response is not None:
        errors = response.data
        message = "An error occurred."
        
        if isinstance(errors, dict) and 'detail' in errors:
            message = errors['detail']
            errors = {"detail": [errors['detail']]}
        elif isinstance(errors, list):
            message = errors[0] if errors else "Validation failed."
            errors = {"non_field_errors": errors}
        elif isinstance(errors, dict):
            for key, val in errors.items():
                if isinstance(val, list) and val:
                    message = f"{key}: {val[0]}"
                elif isinstance(val, str):
                    message = val
                break

        response.data = {
            "status": "error",
            "message": message,
            "errors": errors
        }
    return response
