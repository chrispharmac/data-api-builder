using System.Text.Json;
using System.Threading.Tasks;
using Azure.DataGateway.Service.Controllers;
using Azure.DataGateway.Service.Exceptions;
using Azure.DataGateway.Service.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.DataGateway.Service.Tests.SqlTests.GraphQLMutationTests
{
    /// <summary>
    /// Base class for GraphQL Mutation tests targetting Sql databases.
    /// </summary>
    [TestClass]
    public abstract class GraphQLMutationTestBase : SqlTestBase
    {

        #region Test Fixture Setup
        protected static GraphQLService _graphQLService;
        protected static GraphQLController _graphQLController;
        #endregion

        #region  Positive Tests

        /// <summary>
        /// <code>Do: </code> Inserts new book and return its id and title
        /// <code>Check: </code> If book with the expected values of the new book is present in the database and
        /// if the mutation query has returned the correct information
        /// </summary>
        public async Task InsertMutation(string dbQuery)
        {
            string graphQLMutationName = "createBook";
            string graphQLMutation = @"
                mutation {
                    createBook(item: { title: ""My New Book"", publisher_id: 1234 }) {
                        id
                        title
                    }
                }
            ";

            string actual = await GetGraphQLResultAsync(graphQLMutation, graphQLMutationName, _graphQLController);
            string expected = await GetDatabaseResultAsync(dbQuery);

            SqlTestHelper.PerformTestEqualJsonStrings(expected, actual);
        }

        /// <summary>
        /// <code>Do: </code> Inserts new review with default content for a Review and return its id and content
        /// <code>Check: </code> If book with the given id is present in the database then
        /// the mutation query will return the review Id with the content of the review added
        /// </summary>
        public async Task InsertMutationForConstantdefaultValue(string dbQuery)
        {
            string graphQLMutationName = "createReview";
            string graphQLMutation = @"
                mutation {
                    createReview(item: { book_id: 1 }) {
                        id
                        content
                    }
                }
            ";

            string actual = await GetGraphQLResultAsync(graphQLMutation, graphQLMutationName, _graphQLController);
            string expected = await GetDatabaseResultAsync(dbQuery);

            SqlTestHelper.PerformTestEqualJsonStrings(expected, actual);
        }

        /// <summary>
        /// <code>Do: </code>Update book in database and return its updated fields
        /// <code>Check: </code>if the book with the id of the edited book and the new values exists in the database
        /// and if the mutation query has returned the values correctly
        /// </summary>
        public async Task UpdateMutation(string dbQuery)
        {
            string graphQLMutationName = "updateBook";
            string graphQLMutation = @"
                mutation {
                    updateBook(id: 1, item: { title: ""Even Better Title"", publisher_id: 2345} ) {
                        title
                        publisher_id
                    }
                }
            ";

            string actual = await GetGraphQLResultAsync(graphQLMutation, graphQLMutationName, _graphQLController);
            string expected = await GetDatabaseResultAsync(dbQuery);

            SqlTestHelper.PerformTestEqualJsonStrings(expected, actual);
        }

        /// <summary>
        /// <code>Do: </code>Delete book by id
        /// <code>Check: </code>if the mutation returned result is as expected and if book by that id has been deleted
        /// </summary>
        public async Task DeleteMutation(string dbQueryForResult, string dbQueryToVerifyDeletion)
        {
            string graphQLMutationName = "deleteBook";
            string graphQLMutation = @"
                mutation {
                    deleteBook(id: 1) {
                        title
                        publisher_id
                    }
                }
            ";

            // query the table before deletion is performed to see if what the mutation
            // returns is correct
            string expected = await GetDatabaseResultAsync(dbQueryForResult);
            string actual = await GetGraphQLResultAsync(graphQLMutation, graphQLMutationName, _graphQLController);

            SqlTestHelper.PerformTestEqualJsonStrings(expected, actual);

            string dbResponse = await GetDatabaseResultAsync(dbQueryToVerifyDeletion);

            using JsonDocument result = JsonDocument.Parse(dbResponse);
            Assert.AreEqual(result.RootElement.GetProperty("count").GetInt64(), 0);
        }

        /// <summary>
        /// <code>Do: </code>run a mutation which mutates a relationship instead of a graphql type
        /// <code>Check: </code>that the insertion of the entry in the appropriate link table was successful
        /// </summary>
        // IGNORE FOR NOW, SEE: Issue #285
        public async Task InsertMutationForNonGraphQLTypeTable(string dbQuery)
        {
            string graphQLMutationName = "addAuthorToBook";
            string graphQLMutation = @"
                mutation {
                    addAuthorToBook(author_id: 123, book_id: 2)
                }
            ";

            await GetGraphQLResultAsync(graphQLMutation, graphQLMutationName, _graphQLController);
            string dbResponse = await GetDatabaseResultAsync(dbQuery);

            using JsonDocument result = JsonDocument.Parse(dbResponse);
            Assert.AreEqual(result.RootElement.GetProperty("count").GetInt64(), 1);
        }

        /// <summary>
        /// <code>Do: </code>a new Book insertion and do a nested querying of the returned book
        /// <code>Check: </code>if the result returned from the mutation is correct
        /// </summary>
        public async Task NestedQueryingInMutation(string dbQuery)
        {
            string graphQLMutationName = "createBook";
            string graphQLMutation = @"
                mutation {
                    createBook(item: {title: ""My New Book"", publisher_id: 1234}) {
                        id
                        title
                        publishers {
                            name
                        }
                    }
                }
            ";

            string actual = await GetGraphQLResultAsync(graphQLMutation, graphQLMutationName, _graphQLController);
            string expected = await GetDatabaseResultAsync(dbQuery);

            SqlTestHelper.PerformTestEqualJsonStrings(expected, actual);
        }

        /// <summary>
        /// Test explicitly inserting a null column
        /// </summary>
        public async Task TestExplicitNullInsert(string dbQuery)
        {
            string graphQLMutationName = "createMagazine";
            string graphQLMutation = @"
                mutation {
                    createMagazine(item: { id: 800, title: ""New Magazine"", issue_number: null }) {
                        id
                        title
                        issue_number
                    }
                }
            ";

            string actual = await GetGraphQLResultAsync(graphQLMutation, graphQLMutationName, _graphQLController);
            string expected = await GetDatabaseResultAsync(dbQuery);

            SqlTestHelper.PerformTestEqualJsonStrings(expected, actual);
        }

        /// <summary>
        /// Test implicitly inserting a null column
        /// </summary>
        public async Task TestImplicitNullInsert(string dbQuery)
        {
            string graphQLMutationName = "createMagazine";
            string graphQLMutation = @"
                mutation {
                    createMagazine(item: {id: 801, title: ""New Magazine 2""}) {
                        id
                        title
                        issue_number
                    }
                }
            ";

            string actual = await GetGraphQLResultAsync(graphQLMutation, graphQLMutationName, _graphQLController);
            string expected = await GetDatabaseResultAsync(dbQuery);

            SqlTestHelper.PerformTestEqualJsonStrings(expected, actual);
        }

        /// <summary>
        /// Test updating a column to null
        /// </summary>
        public async Task TestUpdateColumnToNull(string dbQuery)
        {
            string graphQLMutationName = "updateMagazine";
            string graphQLMutation = @"
                mutation {
                    updateMagazine(id: 1, item: { issue_number: null} ) {
                        id
                        issue_number
                    }
                }
            ";

            string actual = await GetGraphQLResultAsync(graphQLMutation, graphQLMutationName, _graphQLController);
            string expected = await GetDatabaseResultAsync(dbQuery);

            SqlTestHelper.PerformTestEqualJsonStrings(expected, actual);
        }

        /// <summary>
        /// Test updating a missing column in the update mutation will not be updated to null
        /// </summary>
        public async Task TestMissingColumnNotUpdatedToNull(string dbQuery)
        {
            string graphQLMutationName = "updateMagazine";
            string graphQLMutation = @"
                mutation {
                    updateMagazine(id: 1, item: {id: 1, title: ""Newest Magazine""}) {
                        id
                        title
                        issue_number
                    }
                }
            ";

            string actual = await GetGraphQLResultAsync(graphQLMutation, graphQLMutationName, _graphQLController);
            string expected = await GetDatabaseResultAsync(dbQuery);

            SqlTestHelper.PerformTestEqualJsonStrings(expected, actual);
        }

        /// <summary>
        /// Test to check graphQL support for aliases(arbitrarily set by user while making request).
        /// book_id and book_title are aliases used for corresponding query fields.
        /// The response for the query will use the alias instead of raw db column.
        /// </summary>
        public async Task TestAliasSupportForGraphQLMutationQueryFields(string dbQuery)
        {
            string graphQLMutationName = "createBook";
            string graphQLMutation = @"
                mutation {
                    createBook(item: { title: ""My New Book"", publisher_id: 1234 }) {
                        book_id: id
                        book_title: title
                    }
                }
            ";

            string actual = await GetGraphQLResultAsync(graphQLMutation, graphQLMutationName, _graphQLController);
            string expected = await GetDatabaseResultAsync(dbQuery);

            SqlTestHelper.PerformTestEqualJsonStrings(expected, actual);
        }

        #endregion

        #region Negative Tests

        /// <summary>
        /// <code>Do: </code>insert a new Book with an invalid foreign key
        /// <code>Check: </code>that GraphQL returns an error and that the book has not actually been added
        /// </summary>
        public static async Task InsertWithInvalidForeignKey(string dbQuery, string errorMessage)
        {
            string graphQLMutationName = "createBook";
            string graphQLMutation = @"
                mutation {
                    createBook(item: { title: ""My New Book"", publisher_id: -1}) {
                        id
                        title
                    }
                }
            ";

            JsonElement result = await GetGraphQLControllerResultAsync(graphQLMutation, graphQLMutationName, _graphQLController);

            SqlTestHelper.TestForErrorInGraphQLResponse(
                result.ToString(),
                message: errorMessage,
                statusCode: $"{DataGatewayException.SubStatusCodes.DatabaseOperationFailed}"
            );

            string dbResponse = await GetDatabaseResultAsync(dbQuery);
            using JsonDocument dbResponseJson = JsonDocument.Parse(dbResponse);
            Assert.AreEqual(dbResponseJson.RootElement.GetProperty("count").GetInt64(), 0);
        }

        /// <summary>
        /// <code>Do: </code>edit a book with an invalid foreign key
        /// <code>Check: </code>that GraphQL returns an error and the book has not been editted
        /// </summary>
        public static async Task UpdateWithInvalidForeignKey(string dbQuery, string errorMessage)
        {
            string graphQLMutationName = "updateBook";
            string graphQLMutation = @"
                mutation {
                    updateBook(id: 1, item: {publisher_id: -1 }) {
                        id
                        title
                    }
                }
            ";

            JsonElement result = await GetGraphQLControllerResultAsync(graphQLMutation, graphQLMutationName, _graphQLController);

            SqlTestHelper.TestForErrorInGraphQLResponse(
                result.ToString(),
                message: errorMessage,
                statusCode: $"{DataGatewayException.SubStatusCodes.DatabaseOperationFailed}"
            );

            string dbResponse = await GetDatabaseResultAsync(dbQuery);
            using JsonDocument dbResponseJson = JsonDocument.Parse(dbResponse);
            Assert.AreEqual(dbResponseJson.RootElement.GetProperty("count").GetInt64(), 0);
        }

        /// <summary>
        /// <code>Do: </code>use an update mutation without passing any of the optional new values to update
        /// <code>Check: </code>check that GraphQL returns an appropriate exception to the user
        /// </summary>
        public virtual async Task UpdateWithNoNewValues()
        {
            string graphQLMutationName = "updateBook";
            string graphQLMutation = @"
                mutation {
                    updateBook(id: 1) {
                        id
                        title
                    }
                }
            ";

            JsonElement result = await GetGraphQLControllerResultAsync(graphQLMutation, graphQLMutationName, _graphQLController);
            SqlTestHelper.TestForErrorInGraphQLResponse(result.ToString(), message: $"item");
        }

        /// <summary>
        /// <code>Do: </code>use an update mutation with an invalid id to update
        /// <code>Check: </code>check that GraphQL returns an appropriate exception to the user
        /// </summary>
        public virtual async Task UpdateWithInvalidIdentifier()
        {
            string graphQLMutationName = "updateBook";
            string graphQLMutation = @"
                mutation {
                    updateBook(id: -1, item: { title: ""Even Better Title"" }) {
                        id
                        title
                    }
                }
            ";

            JsonElement result = await GetGraphQLControllerResultAsync(graphQLMutation, graphQLMutationName, _graphQLController);
            SqlTestHelper.TestForErrorInGraphQLResponse(result.ToString(), statusCode: $"{DataGatewayException.SubStatusCodes.EntityNotFound}");
        }

        /// <summary>
        /// Test adding a website placement to a book which already has a website
        /// placement
        /// </summary>
        public static async Task TestViolatingOneToOneRelashionShip(string errorMessage)
        {
            string graphQLMutationName = "createBookWebsitePlacement";
            string graphQLMutation = @"
                mutation {
                    createBookWebsitePlacement(item: {book_id: 1, price: 25 }) {
                        id
                    }
                }
            ";

            JsonElement result = await GetGraphQLControllerResultAsync(graphQLMutation, graphQLMutationName, _graphQLController);
            SqlTestHelper.TestForErrorInGraphQLResponse(
                result.ToString(),
                message: errorMessage,
                statusCode: $"{DataGatewayException.SubStatusCodes.DatabaseOperationFailed}"
            );
        }
        #endregion
    }
}