package dataservice;

import org.junit.Before;

public class TestData {

    protected String DataServices_BaseUrl;

    @Before
    public void SetBaseUrl(){

        DataServices_BaseUrl = System.getenv("TM_DATASERVICES_URL");

    }

}
