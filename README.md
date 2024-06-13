# chat-app

CLASS DIAGRAM
```mermaid

classDiagram
    class User {
        int userID
        String username
        String email
        String password
        String profileInfo
        createAccount()
        sendFriendRequest(receiverID: int)
        rejectFriendRequest(senderID: int)
        viewFriendList()
        searchUserByID(userID: int)
        editProfile()
    }

    class Admin {
        editUserProfile(userID: int)
        deleteUserProfile(userID: int)
    }

    class FriendRequest {
        int requestID
        int senderID
        int receiverID
        String status
        accept()
        reject()
    }

    class FriendList {
        int userID
        List<int> friends
        searchFriendByID(friendID: int)
        initiateConversation(friendID: int)
    }

    class Conversation {
        int conversationID
        List<int> participants
        List<Message> messages
        sendMessage(message: String)
        receiveMessage()
    }

    class Message {
        int messageID
        int senderID
        int receiverID
        String content
        DateTime timestamp
    }

    User <|-- Admin
    User "1" --o "many" FriendRequest : sends/receives
    User "1" --o "1" FriendList : has
    User "many" --o "many" Conversation : participates
    Conversation "1" --o "many" Message : contains

```

  
  
USER STORY

* As an user, I want to be able to create a conversation with another user  
* As an user, I want to be able to see all my friend in the friends list
* As a user I want to be able to register/login on the app
* As a registered user I want to send messages to other users
* As an user I want to be able to search other users on the app
* As an user, I want to be able to accept or decline a received invitation
* As an admin, I want to be able to edit the profile of other users  
* As an user, I want the message to be marked as seen when I open it
* As an user I want to be able to search my friends in my friends list
* As an admin, I want to be able to remove users from the app  



JIRA - 


Raportare bug: https://stefanpoleac.atlassian.net/browse/TASK-24  
