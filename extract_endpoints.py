import yaml
import csv

with open('schema.yml', 'r') as f:
    schema = yaml.safe_load(f)

with open('endpoint_inventory.csv', 'w', newline='') as f:
    writer = csv.writer(f)
    writer.writerow([
        'Path', 'Method', 'Auth Requirement', 'Params (Path/Query)', 
        'Request Schema', 'Response Schema', 'Status Codes', 
        'Rate Limit Tier', 'Deprecation Status'
    ])
    
    paths = schema.get('paths', {})
    for path, methods in paths.items():
        for method, details in methods.items():
            if method not in ['get', 'post', 'put', 'patch', 'delete', 'options', 'head']:
                continue
            
            # Auth
            security = details.get('security', [])
            auth = 'None'
            if security:
                auth_types = []
                for sec in security:
                    auth_types.extend(sec.keys())
                auth = ', '.join(set(auth_types)) if auth_types else 'None'
            
            # Params
            params = details.get('parameters', [])
            param_names = [f"{p.get('name')} ({p.get('in')})" for p in params]
            params_str = ', '.join(param_names) if param_names else 'None'
            
            # Request Schema
            req_body = details.get('requestBody', {}).get('content', {}).get('application/json', {}).get('schema', {})
            req_schema = req_body.get('$ref', 'None').split('/')[-1] if req_body else 'None'
            
            # Responses
            responses = details.get('responses', {})
            status_codes = ', '.join(responses.keys())
            
            # Try to get 200/201 schema
            resp_schema = 'None'
            for code in ['200', '201']:
                if code in responses:
                    content = responses[code].get('content', {}).get('application/json', {}).get('schema', {})
                    if content:
                        if '$ref' in content:
                            resp_schema = content['$ref'].split('/')[-1]
                        elif 'type' in content:
                            resp_schema = content['type']
            
            # Rate limit (not standard in openapi, just placeholder)
            rate_limit = 'Unknown'
            
            # Deprecated
            deprecated = str(details.get('deprecated', False))
            
            writer.writerow([
                path, method.upper(), auth, params_str, 
                req_schema, resp_schema, status_codes, 
                rate_limit, deprecated
            ])
print("Done")
