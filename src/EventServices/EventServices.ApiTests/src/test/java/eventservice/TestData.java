package eventservice;

import com.atlassian.oai.validator.restassured.OpenApiValidationFilter;
import org.junit.Before;

public class TestData {

    protected String EventServices_BaseUrl;
    protected OpenApiValidationFilter EventServices_ValidationFilter;

    @Before
    public void SetBaseUrl() {

        EventServices_BaseUrl = System.getenv("TM_EVENTSERVICES_URL");

        String EventServices_SwaggerDef = EventServices_BaseUrl + "/swagger-original.json";
        EventServices_ValidationFilter = new OpenApiValidationFilter(EventServices_SwaggerDef);

    }

}
