importScripts("https://www.gstatic.com/firebasejs/7.16.1/firebase-app.js");
importScripts("https://www.gstatic.com/firebasejs/7.16.1/firebase-messaging.js");
// For an optimal experience using Cloud Messaging, also add the Firebase SDK for Analytics.
importScripts("https://www.gstatic.com/firebasejs/7.16.1/firebase-analytics.js");

self.addEventListener('install', async event => {
    console.log('Installing firebase cloud messaging service worker...');
    await self.skipWaiting();
});

// Initialize the Firebase app in the service worker by passing in the messagingSenderId.
firebase.initializeApp({
    apiKey: "AIzaSyCbSDI-E1HgNTuZiFVPoL0yOJ-DD-P_rDE",
    authDomain: "ethachat-2023.firebaseapp.com",
    projectId: "ethachat-2023",
    storageBucket: "ethachat-2023.appspot.com",
    messagingSenderId: "383190008660",
    appId: "1:383190008660:web:b9045fa8b74f2ab969fec1"
});

// Retrieve an instance of Firebase Messaging so that it can handle background messages.
const messaging = firebase.messaging();

messaging.setBackgroundMessageHandler(function(payload) {
    console.log(
        "[firebase-messaging-sw.js] Received background message ",
        payload,
    );
    // Customize notification here
    const notificationTitle = "Background Message Title";
    const notificationOptions = {
        body: "Background Message body.",
        icon: "/itwonders-web-logo.png",
    };

    return self.registration.showNotification(
        notificationTitle,
        notificationOptions,
    );
});