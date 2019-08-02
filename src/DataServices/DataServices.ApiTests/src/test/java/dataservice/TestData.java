package dataservice;

import com.atlassian.oai.validator.restassured.OpenApiValidationFilter;
import org.junit.Before;

public class TestData {

    protected String DataServices_BaseUrl;
    protected OpenApiValidationFilter DataServices_ValidationFilter;

    @Before
    public void SetBaseUrl() {

        DataServices_BaseUrl = System.getenv("TM_DATASERVICES_URL");

        String DataServices_SwaggerDef = DataServices_BaseUrl + "/swagger-original.json";
        DataServices_ValidationFilter = new OpenApiValidationFilter(DataServices_SwaggerDef);

    }

}
