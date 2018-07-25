package database;

import com.amazonaws.AmazonClientException;
import com.amazonaws.AmazonServiceException;
import com.amazonaws.auth.profile.ProfileCredentialsProvider;
import com.amazonaws.services.dynamodbv2.AmazonDynamoDB;
import com.amazonaws.services.dynamodbv2.AmazonDynamoDBClientBuilder;
import com.amazonaws.services.dynamodbv2.document.DynamoDB;
import com.amazonaws.services.dynamodbv2.document.Item;
import com.amazonaws.services.dynamodbv2.document.Table;
import com.amazonaws.services.dynamodbv2.model.*;
import com.amazonaws.services.dynamodbv2.util.TableUtils;

import java.util.HashMap;
import java.util.Map;

public class MSSManager {

	private AmazonDynamoDB dynamoDBClient;
	private DynamoDB dynamoDB;
	private final String tableName = "metrics";
	private final double INTERVAL_SIZE = 25.0;

	public MSSManager() throws Exception {
		this.dynamoDBClient = initClient();
		this.dynamoDB = new DynamoDB(dynamoDBClient);
		initDatabase();
	}

	private AmazonDynamoDB initClient() throws Exception {

		ProfileCredentialsProvider credentialsProvider = new ProfileCredentialsProvider();
		try {
			credentialsProvider.getCredentials();
		} catch (Exception e) {
			throw new AmazonClientException(
					"Cannot load the credentials from the credential profiles file. " +
							"Please make sure that your credentials file is at the correct " +
							"location (~/.aws/credentials), and is in valid format.",
					e);
		}
		AmazonDynamoDB dynamoDB = AmazonDynamoDBClientBuilder.standard()
				.withCredentials(credentialsProvider)
				.withRegion("us-east-1")
				.build();

		return dynamoDB;
	}

	public void initDatabase() throws InterruptedException {
		try {
			// Create a table with a primary hash key named 'name', which holds a string
			CreateTableRequest createTableRequest = new CreateTableRequest().withTableName(tableName)
					.withKeySchema(new KeySchemaElement().withAttributeName("pk").withKeyType(KeyType.HASH))
					.withAttributeDefinitions(new AttributeDefinition().withAttributeName("pk").withAttributeType(ScalarAttributeType.N))
					.withProvisionedThroughput(new ProvisionedThroughput().withReadCapacityUnits(1L).withWriteCapacityUnits(1L));

			// Create table if it does not exist yet
			TableUtils.createTableIfNotExists(dynamoDBClient, createTableRequest);
			// wait for the table to move into ACTIVE state
			TableUtils.waitUntilActive(dynamoDBClient, tableName);

			// Describe our new table
			DescribeTableRequest describeTableRequest = new DescribeTableRequest().withTableName(tableName);
			TableDescription tableDescription = dynamoDBClient.describeTable(describeTableRequest).getTable();
			System.out.println("Table Description: " + tableDescription);

		} catch (AmazonServiceException ase) {
			System.out.println("Caught an AmazonServiceException, which means your request made it "
					+ "to AWS, but was rejected with an error response for some reason.");
			System.out.println("Error Message:    " + ase.getMessage());
			System.out.println("HTTP Status Code: " + ase.getStatusCode());
			System.out.println("AWS Error Code:   " + ase.getErrorCode());
			System.out.println("Error Type:       " + ase.getErrorType());
			System.out.println("Request ID:       " + ase.getRequestId());
		} catch (AmazonClientException ace) {
			System.out.println("Caught an AmazonClientException, which means the client encountered "
					+ "a serious internal problem while trying to communicate with AWS, "
					+ "such as not being able to access the network.");
			System.out.println("Error Message: " + ace.getMessage());
		}
	}

	private String getVelocityInterval(String velocity) {
		int vel = Integer.parseInt(velocity);

		double myInterval = vel/INTERVAL_SIZE;
		int upperIntegerValue = (int) Math.ceil(myInterval);

		System.out.println("Getting Velocity Interval: " + upperIntegerValue);

		return ""+upperIntegerValue;
	}

	public int getHashCode(String key1, String key2, String key3){
		StringBuilder builder = new StringBuilder();
		
		builder.append(key1);
		builder.append(key2);
		builder.append(getVelocityInterval(key3));

		return builder.toString().hashCode();
	}

	public Item getItemByPK(int pk){
		Table table = dynamoDB.getTable(tableName);

		Item item = table.getItem("pk", pk);

		return item;
	}

	private Map<String, AttributeValue> newItem(int pk, String maze, String strat, String vel, long metric, int updates) {
		Map<String, AttributeValue> item = new HashMap<>();
		item.put("pk", new AttributeValue().withN(Integer.toString(pk)));
		item.put("maze", new AttributeValue(maze));
		item.put("strategy", new AttributeValue(strat));
		item.put("velocity", new AttributeValue().withS(vel));
		item.put("metric", new AttributeValue().withN(Long.toString(metric)));
		item.put("updates", new AttributeValue().withN(Integer.toString(updates)));

		return item;
	}

	public void addItem(String maze, String strat, String vel, long metric){
		int updates = 0;

		int pk = getHashCode(maze,strat,vel);
		Item itemOnDB = getItemByPK(pk);

		if(itemOnDB != null) {
			System.out.println("Item already exists in db, has pk: " + pk);
			updates = itemOnDB.getInt("updates");
			metric = (itemOnDB.getLong("metric")*updates + metric)/(updates+1);
		} else {
			System.out.println("Item doesn't exist in db, has pk: " + pk);
		}

		Map<String, AttributeValue> item = newItem(pk, maze, strat, getVelocityInterval(vel), metric,updates+1);
		PutItemRequest putItemRequest = new PutItemRequest(tableName, item);
		PutItemResult putItemResult = dynamoDBClient.putItem(putItemRequest);
	}
}