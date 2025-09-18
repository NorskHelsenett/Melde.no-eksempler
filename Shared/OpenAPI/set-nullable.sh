# This script sets FeilMeldingViewModel as nullable due to a 'bug' in Swagger generator

sed -i 's;"#/components/schemas/Melde.DTO.Response.FeilmeldingViewModel";"#/components/schemas/Melde.DTO.Response.FeilmeldingViewModel", "nullable": true;g' swagger-v1.json
sed -i 's;"#/components/schemas/Melde.DTO.Response.FeilmeldingViewModel";"#/components/schemas/Melde.DTO.Response.FeilmeldingViewModel", "nullable": true;g' swagger-v2.json
